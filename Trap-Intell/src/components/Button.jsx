function Button({ className, id, children }) {
  return (
    <button className={className} id={id}>
      {children}
    </button>
  );
}

export default Button;
